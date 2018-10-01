using Sitecore.Data;
using Sitecore.Data.Events;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Events;
using Sitecore.Web.UI.Sheer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sitecore.Support.Buckets.Pipelines.UI
{
  public class ItemDuplicate : Sitecore.Buckets.Pipelines.UI.ItemDuplicate
  {
    public new void Execute(ClientPipelineArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      Database arg_24_0 = this.ExtractDatabase(args);
      string path = args.Parameters["id"];
      Item item = arg_24_0.GetItem(path);
      if (item == null)
      {
        this.ShowLocalizedAlert("Item not found.", Array.Empty<object>());
        args.AbortPipeline();
      }
      else
      {
        Item parent = item.Parent;
        if (parent == null)
        {
          this.ShowLocalizedAlert("Cannot duplicate the root item.", Array.Empty<object>());
          args.AbortPipeline();
        }
        else if (parent.Access.CanCreate())
        {
          Log.Audit(this, "Duplicate item: {0}", new string[]
          {
                AuditFormatter.FormatItem(item)
          });
          Item parentBucketItemOrSiteRoot = this.GetParentBucketItemOrSiteRoot(item);
          if (this.IsBucket(parentBucketItemOrSiteRoot) && this.IsBucketable(item))
          {
            if (!EventDisabler.IsActive)
            {
              EventResult eventResult = Event.RaiseEvent("item:bucketing:duplicating", new object[]
              {
                        args,
                        this
              });
              if (eventResult != null && eventResult.Cancel)
              {
                Log.Info(string.Format("Event {0} was cancelled", "item:bucketing:duplicating"), this);
                args.AbortPipeline();
                return;
              }
            }
            Item item2 = this.DuplicateItem(item, args.Parameters["name"]);
            Item destination = this.CreateAndReturnBucketFolderDestination(parentBucketItemOrSiteRoot, DateUtil.ToUniversalTime(DateTime.Now), item2);
            #region patch.149714
            if (!this.IsBucketTemplateCheck(item))
            #endregion
            {
              destination = parentBucketItemOrSiteRoot;
            }
            this.MoveItem(item2, destination);
            if (!EventDisabler.IsActive)
            {
              Event.RaiseEvent("item:bucketing:duplicated", new object[]
              {
                        args,
                        this
              });
            }
          }
          else
          {
            this.DuplicateItem(item, args.Parameters["name"]);
          }
        }
        else
        {
          this.ShowLocalizedAlert("You do not have permission to duplicate \"{0}\".", new object[]
          {
                item.DisplayName
          });
          args.AbortPipeline();
        }
      }
      args.AbortPipeline();
    }

    private bool IsBucketTemplateCheck(Item item)
    {
      if (item != null)
      {
        if (item.Fields[Sitecore.Buckets.Util.Constants.IsBucket] != null)
        {
          return item.Fields[Sitecore.Buckets.Util.Constants.BucketableField].Value.Equals("1");
        }
        if (item.Paths.FullPath.StartsWith("/sitecore/templates"))
        {
          TemplateItem templateItem = (item.Children[0] != null) ? item.Children[0].Template : null;
          if (templateItem != null)
          {
            TemplateItem templateItem2 = new TemplateItem(templateItem);
            if (templateItem.StandardValues != null && templateItem2.StandardValues[Sitecore.Buckets.Util.Constants.BucketableField] != null)
            {
              return templateItem2.StandardValues[Sitecore.Buckets.Util.Constants.BucketableField].Equals("1");
            }
          }
        }
      }
      return false;
    }
  }
}